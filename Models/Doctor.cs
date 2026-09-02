using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Clinic_Application_Doctor_Management.Models{
    public class Doctor{
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string Specialization { get; set; } = string.Empty;

        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [StringLength(20)]
        public string Phone { get; set; } = string.Empty;

        [StringLength(200)]
        public string Qualification { get; set; } = string.Empty;

        [Range(0, 60)]
        public int Experience { get; set; }

        [StringLength(500)]
        public string? About { get; set; }

        public string? ProfilePicture { get; set; }

        public ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();
        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
        public ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
    }
}