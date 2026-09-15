using System;
using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.ViewModels
{
    public class AdminAppointmentViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Appointment Code")]
        public string AppointmentCode { get; set; } = string.Empty;

        [Display(Name = "Patient Name")]
        public string PatientName { get; set; } = string.Empty;

        [Display(Name = "Patient Code")]
        public string PatientCode { get; set; } = string.Empty;

        [Display(Name = "Patient Phone")]
        public string PatientPhone { get; set; } = string.Empty;

        [Display(Name = "Doctor Name")]
        public string DoctorName { get; set; } = string.Empty;

        [Display(Name = "Specialization")]
        public string DoctorSpecialization { get; set; } = string.Empty;

        public string? DoctorProfilePicture { get; set; }

        [Display(Name = "Appointment Date")]
        [DataType(DataType.Date)]
        public DateTime AppointmentDate { get; set; }

        [Display(Name = "Appointment Time")]
        public TimeSpan AppointmentTime { get; set; }

        public string Reason { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public int DurationMinutes { get; set; }

        [Display(Name = "Booked On")]
        public DateTime CreatedAt { get; set; }
    }
}