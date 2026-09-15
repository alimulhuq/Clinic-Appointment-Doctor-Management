using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Clinic_Application_Doctor_Management.Data;
using Clinic_Application_Doctor_Management.Models;
using Clinic_Application_Doctor_Management.Services.Interfaces;

namespace Clinic_Application_Doctor_Management.Services.Implementations
{
    public class AppointmentService : IAppointmentService
    {
        private readonly ApplicationDbContext _context;
        public AppointmentService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<(bool IsValid, string? ErrorMessage)> ValidateAppointmentAsync(Appointment appointment)
        {
            int duration = appointment.DurationMinutes > 0 ? appointment.DurationMinutes : 30;
            DateTime startDateTime = appointment.AppointmentDate.Date.Add(appointment.AppointmentTime);
            DateTime endDateTime = startDateTime.AddMinutes(duration);

            // 1. Past check
            if (startDateTime < DateTime.Now)
                return (false, "Appointment date and time cannot be in the past.");

            // 2. Doctor availability — match on day of week and time within [Start, End]
            var dayOfWeek = startDateTime.DayOfWeek;
            var apptStart = startDateTime.TimeOfDay;
            var apptEnd = endDateTime.TimeOfDay;

            var schedule = await _context.Schedules
                .FirstOrDefaultAsync(s => s.DoctorId == appointment.DoctorId
                    && s.DayOfWeek == dayOfWeek
                    && s.IsActive
                    && s.StartTime <= apptStart
                    && s.EndTime >= apptEnd);

            if (schedule == null)
                return (false, "Doctor is not available at the selected date and time.");

            // 3. Overlap with same doctor's other appointments
            bool doctorConflict = await _context.Appointments
                .AnyAsync(a => a.DoctorId == appointment.DoctorId
                    && a.Id != appointment.Id
                    && a.Status != "Cancelled"
                    && a.AppointmentDate.Add(a.AppointmentTime) < endDateTime
                    && startDateTime < a.AppointmentDate.Add(a.AppointmentTime)
                        .Add(TimeSpan.FromMinutes(a.DurationMinutes > 0 ? a.DurationMinutes : 30)));

            if (doctorConflict)
                return (false, "This time slot is already booked with this doctor.");

            // 4. Patient overlapping appointments
            bool patientConflict = await _context.Appointments
                .AnyAsync(a => a.PatientId == appointment.PatientId
                    && a.Id != appointment.Id
                    && a.Status != "Cancelled"
                    && a.AppointmentDate.Add(a.AppointmentTime) < endDateTime
                    && startDateTime < a.AppointmentDate.Add(a.AppointmentTime)
                        .Add(TimeSpan.FromMinutes(a.DurationMinutes > 0 ? a.DurationMinutes : 30)));

            if (patientConflict)
                return (false, "You already have an overlapping appointment at that time.");

            return (true, null);
        }
    }
}