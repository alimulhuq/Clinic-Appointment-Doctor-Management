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

            // Start / end of the new appointment
            DateTime startDateTime = appointment.AppointmentDate.Date.Add(appointment.AppointmentTime);
            DateTime endDateTime = startDateTime.AddMinutes(duration);

            // 1. Past check
            if (startDateTime < DateTime.Now)
                return (false, "Appointment date and time cannot be in the past.");

            // 2. Doctor availability (schedule check) — fully translatable to SQL
            var dayOfWeek = startDateTime.DayOfWeek;
            var apptStartTime = startDateTime.TimeOfDay;
            var apptEndTime = endDateTime.TimeOfDay;

            var schedule = await _context.Schedules
                .FirstOrDefaultAsync(s => s.DoctorId == appointment.DoctorId
                    && s.DayOfWeek == dayOfWeek
                    && s.IsActive
                    && s.StartTime <= apptStartTime
                    && s.EndTime >= apptEndTime);

            if (schedule == null)
                return (false, "Doctor is not available at the selected date and time.");

            // 3. Pull overlapping candidates into memory (SQL can't translate DateTime.Add)
            //    Filter at DB level by date first — cheap and indexable.
            var candidateAppointments = await _context.Appointments
                .Where(a => a.Id != appointment.Id
                    && a.Status != "Cancelled"
                    && a.AppointmentDate.Date == appointment.AppointmentDate.Date
                    && (a.DoctorId == appointment.DoctorId || a.PatientId == appointment.PatientId))
                .ToListAsync();

            // 4. Doctor conflict — same doctor, overlapping time (client-side)
            bool doctorConflict = candidateAppointments.Any(a =>
            {
                if (a.DoctorId != appointment.DoctorId) return false;
                var existingStart = a.AppointmentDate.Date.Add(a.AppointmentTime);
                var existingEnd = existingStart.AddMinutes(a.DurationMinutes > 0 ? a.DurationMinutes : 30);
                return existingStart < endDateTime && startDateTime < existingEnd;
            });

            if (doctorConflict)
                return (false, "This time slot is already booked with this doctor.");

            // 5. Patient conflict — patient already has overlapping appointment (client-side)
            bool patientConflict = candidateAppointments.Any(a =>
            {
                if (a.PatientId != appointment.PatientId) return false;
                var existingStart = a.AppointmentDate.Date.Add(a.AppointmentTime);
                var existingEnd = existingStart.AddMinutes(a.DurationMinutes > 0 ? a.DurationMinutes : 30);
                return existingStart < endDateTime && startDateTime < existingEnd;
            });

            if (patientConflict)
                return (false, "You already have an overlapping appointment at that time.");

            return (true, null);
        }
    }
}